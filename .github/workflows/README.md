# GitHub Actions CI/CD Workflows

This directory contains GitHub Actions workflows for the NexusFS project.

## Workflows

### 1. CI/CD Pipeline (`ci-cd.yml`)

Main pipeline that runs on every push and pull request.

**Jobs:**
- **build-and-test**: Builds the application, runs unit tests, and generates code coverage
- **docker-build-push**: Builds and pushes Docker images to GitHub Container Registry
- **deploy-staging**: Deploys to staging environment (develop branch)
- **deploy-production**: Deploys to production environment (main branch or releases)
- **cleanup**: Removes old container images

**Triggers:**
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop`
- Release published

### 2. Security Scan (`security-scan.yml`)

Automated security scanning workflow.

**Jobs:**
- **dependency-scan**: Scans dependencies for known vulnerabilities
- **code-analysis**: Static code analysis with CodeQL
- **secret-scan**: Scans for exposed secrets using TruffleHog

**Triggers:**
- Weekly schedule (Sunday at midnight)
- Manual trigger via workflow_dispatch

## Setup Instructions

### Required Secrets

Configure the following secrets in your GitHub repository settings:

#### Kubernetes Configuration
- `KUBE_CONFIG_STAGING`: Base64-encoded kubeconfig for staging cluster
- `KUBE_CONFIG_PRODUCTION`: Base64-encoded kubeconfig for production cluster

To generate:
```bash
cat ~/.kube/config | base64 -w 0
```

#### Notifications (Optional)
- `SLACK_WEBHOOK`: Slack webhook URL for deployment notifications

### Environment Configuration

Create the following environments in your repository settings:

1. **staging**
   - URL: https://staging-api.nexusfs.example.com
   - Protection rules: Require reviewers (optional)

2. **production**
   - URL: https://api.nexusfs.example.com
   - Protection rules: Require reviewers (recommended)

### Container Registry

The workflow uses GitHub Container Registry (ghcr.io) by default. Images are pushed to:
```
ghcr.io/<your-org>/<your-repo>/nexusfs-api
```

Ensure the `GITHUB_TOKEN` has `packages: write` permission.

## Customization

### Changing Container Registry

To use a different registry (Docker Hub, AWS ECR, etc.):

1. Update the `DOCKER_REGISTRY` and `IMAGE_NAME` environment variables in `ci-cd.yml`
2. Update the login action with appropriate credentials
3. Add registry credentials as secrets

### Modifying Deployment Strategy

The current deployment uses `kubectl set image` for rolling updates. To use different strategies:

- **Blue-Green**: Modify deployment steps to create new deployment, test, then switch service
- **Canary**: Use Flagger or Argo Rollouts for progressive delivery
- **Helm**: Replace kubectl commands with Helm upgrade commands

### Adding Additional Tests

Add test jobs in the `build-and-test` job:

```yaml
- name: Run integration tests
  run: dotnet test be-nexus-fs/IntegrationTests --configuration Release
```

### Notifications

Add notification steps to any job:

```yaml
- name: Notify on failure
  uses: 8398a7/action-slack@v3
  with:
    status: failure
    webhook_url: ${{ secrets.SLACK_WEBHOOK }}
  if: failure()
```

## Monitoring

### Workflow Status

View workflow runs:
- Go to **Actions** tab in your repository
- Click on a workflow to see detailed logs

### Deployment Status

Check deployment status:
```bash
kubectl rollout status deployment/nexusfs-api -n nexusfs
```

### Rollback

If a deployment fails, rollback:
```bash
kubectl rollout undo deployment/nexusfs-api -n nexusfs
```

## Best Practices

1. **Branch Protection**: Enable branch protection on `main` and `develop`
2. **Required Checks**: Make CI/CD workflow a required check for merging
3. **Code Review**: Require at least one approval before merging
4. **Secrets Rotation**: Regularly rotate all secrets and credentials
5. **Dependency Updates**: Use Dependabot to keep dependencies up to date
6. **Security Scanning**: Review security scan results regularly
7. **Monitoring**: Set up alerts for failed deployments
8. **Documentation**: Keep this README updated with any workflow changes

## Troubleshooting

### Build Failures

1. Check the build logs in the Actions tab
2. Verify all dependencies are correctly specified
3. Ensure .NET version matches project requirements

### Deployment Failures

1. Check kubectl logs: `kubectl logs -f deployment/nexusfs-api -n nexusfs`
2. Verify Kubernetes secrets are correctly configured
3. Check pod status: `kubectl get pods -n nexusfs`
4. Review events: `kubectl get events -n nexusfs --sort-by='.lastTimestamp'`

### Image Push Failures

1. Verify GITHUB_TOKEN has packages:write permission
2. Check Docker build logs for errors
3. Ensure Dockerfile is valid and builds locally

## Support

For issues or questions:
1. Check workflow logs in the Actions tab
2. Review Kubernetes pod logs
3. Consult the main project documentation
4. Open an issue in the repository

